# ansible
ansible deploy bot

```sh
echo "${YOU_MUST_KNOWN_ANSIBLE_VAULT_PASSWORD}" > .vault_pass.txt
python3 -m pip install -r requirements.txt
ansible-galaxy install -r requirements.yml
ansible-playbook --limit test-server playbooks/deploy.yml
ansible-playbook --limit prod-server playbooks/deploy.yml
```
